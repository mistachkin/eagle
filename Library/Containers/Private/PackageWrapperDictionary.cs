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

using Eagle._Attributes;
using Eagle._Containers.Public;

using PackageWrapper = Eagle._Wrappers.Package;

using PackagePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Package>;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary of package wrappers, keyed by name.
    /// It extends the generic wrapper dictionary with a type name suitable for
    /// use within Eagle and the ability to produce a list of package names
    /// paired with their available versions.
    /// </summary>
    [ObjectId("cea47a6d-b9e1-4bdd-b1c7-1dc0be1967af")]
    internal sealed class PackageWrapperDictionary :
            WrapperDictionary<string, PackageWrapper>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public PackageWrapperDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Builds a list that pairs each package name in this dictionary with a
        /// string describing the versions for which that package can be
        /// provided.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the version strings, or null to include
        /// all of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A list containing each package name followed by its associated
        /// version string, or null if this dictionary is empty.
        /// </returns>
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

                PackageWrapper wrapper = pair.Value;

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
