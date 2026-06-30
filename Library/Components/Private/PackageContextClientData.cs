/*
 * PackageContextClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using PackageWrapper = Eagle._Wrappers.Package;

using PackagePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Package>;

using PackageIndexPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Containers.Private.PackageWrapperDictionary>;

using PackageIndexWrapperDictionary = Eagle._Containers.Public.PathDictionary<
    Eagle._Containers.Private.PackageWrapperDictionary>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class represents the client data used to track the packages
    /// associated with one or more package index files, providing the context
    /// needed to look up, add, and enumerate packages while processing a
    /// package index.
    /// </summary>
    [ObjectId("47a703f1-8707-44f9-8d92-39c7ce167925")]
    internal sealed class PackageContextClientData : ClientData
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class wrapping the specified data and
        /// initializing an empty collection of indexed packages.
        /// </summary>
        /// <param name="data">
        /// The data to wrap.  This parameter may be null.
        /// </param>
        private PackageContextClientData(
            object data
            )
            : base(data)
        {
            indexedPackages = new PackageIndexWrapperDictionary();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no wrapped data and an
        /// empty collection of indexed packages.
        /// </summary>
        public PackageContextClientData()
            : this(null)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method gets the collection of packages associated with the
        /// specified file name, optionally creating a new, empty collection
        /// when none yet exists.
        /// </summary>
        /// <param name="fileName">
        /// The package index file name whose packages are to be obtained.
        /// </param>
        /// <param name="create">
        /// Non-zero to create a new, empty collection of packages when none is
        /// currently associated with the specified file name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the
        /// packages could not be obtained.
        /// </param>
        /// <returns>
        /// The collection of packages associated with the specified file name,
        /// or null if it could not be obtained.
        /// </returns>
        private PackageWrapperDictionary GetPackages(
            string fileName, /* in */
            bool create,     /* in */
            ref Result error /* out */
            )
        {
            if (fileName == null)
            {
                error = "invalid file name";
                return null;
            }

            if (indexedPackages == null)
            {
                error = "indexed packages not available";
                return null;
            }

            PackageWrapperDictionary packages;

            if (!indexedPackages.TryGetValue(fileName, out packages))
            {
                if (create)
                {
                    packages = new PackageWrapperDictionary();
                    indexedPackages.Add(fileName, packages);
                }
                else
                {
                    error = "missing packages for file name";
                    return null;
                }
            }
            else if (packages == null)
            {
                error = "invalid packages for file name";
                return null;
            }

            return packages;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a flat list that pairs each indexed package file
        /// name with the names and versions of the packages associated with it,
        /// optionally filtered by a pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the package names and versions, or null
        /// to include all of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of file names and their associated package names and
        /// versions, or null if there are no indexed packages.
        /// </returns>
        private StringList ToList(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = null;

            if (indexedPackages != null)
            {
                foreach (PackageIndexPair pair in indexedPackages)
                {
                    StringList subList = null;
                    PackageWrapperDictionary value = pair.Value;

                    if (value != null)
                    {
                        subList = value.NamesAndVersions(
                            pattern, noCase);
                    }

                    if (list == null)
                        list = new StringList();

                    list.Add(pair.Key);

                    list.Add((subList != null) ?
                        subList.ToString() : null);
                }
            }

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The file name of the package index currently being processed.
        /// </summary>
        private string indexFileName;
        /// <summary>
        /// Gets the file name of the package index currently being processed.
        /// </summary>
        public string IndexFileName
        {
            get { return indexFileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The collection of packages indexed by their package index file name.
        /// </summary>
        private PackageIndexWrapperDictionary indexedPackages;
        /// <summary>
        /// Gets the collection of packages indexed by their package index file
        /// name.
        /// </summary>
        public PackageIndexWrapperDictionary IndexedPackages
        {
            get { return indexedPackages; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method changes the file name of the package index currently
        /// being processed, optionally resetting any packages already
        /// associated with it.
        /// </summary>
        /// <param name="fileName">
        /// The new package index file name.
        /// </param>
        /// <param name="reset">
        /// Non-zero to clear any packages already associated with the specified
        /// file name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the index
        /// file name could not be changed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode ChangeIndexFileName(
            string fileName, /* in */
            bool reset,      /* in */
            ref Result error /* out */
            )
        {
            PackageWrapperDictionary packages = GetPackages(
                fileName, true, ref error);

            if (packages == null)
                return ReturnCode.Error;

            if (reset && (packages.Count > 0))
            {
                /* NO RESULT */
                packages.Clear();
            }

            indexFileName = fileName;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any packages are associated with the
        /// package index file currently being processed.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the
        /// packages could not be obtained.
        /// </param>
        /// <returns>
        /// True if packages are associated with the current index file name;
        /// otherwise, false.
        /// </returns>
        public bool HasPackages(
            ref Result error /* out */
            )
        {
            PackageWrapperDictionary packages = GetPackages(
                indexFileName, false, ref error);

            return (packages != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up a package by name within the package index file
        /// currently being processed.
        /// </summary>
        /// <param name="name">
        /// The name of the package to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the package is looked up and validated.
        /// </param>
        /// <param name="package">
        /// Upon success, receives the package (or its wrapper) matching the
        /// specified name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the
        /// package could not be looked up.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode GetPackage(
            string name,             /* in */
            LookupFlags lookupFlags, /* in */
            ref IPackage package,    /* out */
            ref Result error         /* out */
            )
        {
            if (name == null)
            {
                error = "invalid package name";
                return ReturnCode.Error;
            }

            PackageWrapperDictionary packages = GetPackages(
                indexFileName, false, ref error);

            if (packages == null)
                return ReturnCode.Error;

            PackageWrapper wrapper;

            if (!packages.TryGetValue(name, out wrapper))
            {
                error = "package not found";
                return ReturnCode.Error;
            }

            if (FlagOps.HasFlags(
                    lookupFlags, LookupFlags.Wrapper, true))
            {
                package = wrapper;
            }
            else
            {
                if (wrapper == null)
                {
                    error = "invalid package wrapper";
                    return ReturnCode.Error;
                }

                package = wrapper.package;
            }

            if ((package == null) && FlagOps.HasFlags(
                    lookupFlags, LookupFlags.Validate, true))
            {
                error = "invalid package";
                return ReturnCode.Error;
            }
            else
            {
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds a package to the package index file currently being
        /// processed, failing if a package with the same name already exists.
        /// </summary>
        /// <param name="package">
        /// The package to add.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the package being added.
        /// </param>
        /// <param name="result">
        /// Upon failure, receives an error message that describes why the
        /// package could not be added.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode AddPackage(
            IPackage package,       /* in */
            IClientData clientData, /* in */
            ref Result result       /* out */
            )
        {
            if (package == null)
            {
                result = "invalid package";
                return ReturnCode.Error;
            }

            string name = package.Name;

            if (name == null)
            {
                result = "invalid package name";
                return ReturnCode.Error;
            }

            PackageWrapperDictionary packages = GetPackages(
                indexFileName, false, ref result);

            if (packages == null)
                return ReturnCode.Error;

            if (packages.ContainsKey(name))
            {
                result = String.Format(
                    "can't add {0}: package already exists",
                    FormatOps.WrapOrNull(name));

                return ReturnCode.Error;
            }

            bool success = false;
            IWrapper wrapper = null;

            try
            {
                long id = EntityOps.NextTokenIdNoThrow(package);

                wrapper = EntityOps.MaybeNewWrapperWith<PackageWrapper>(
                    id, package);

                if (wrapper == null)
                {
                    result = String.Format(
                        "can't add {0}: no wrapper",
                        FormatOps.WrapOrNull(name));

                    return ReturnCode.Error;
                }

                packages.Add(name, wrapper as PackageWrapper);
                success = true;

                return ReturnCode.Ok;
            }
            finally
            {
                if (!success && (wrapper != null))
                {
                    wrapper.Dispose();
                    wrapper = null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of the indexed packages,
        /// pairing each package index file name with the names and versions of
        /// its packages.
        /// </summary>
        /// <returns>
        /// The string representation of the indexed packages, or null if there
        /// are no indexed packages.
        /// </returns>
        public override string ToString()
        {
            StringList list = ToList(null, false);

            return (list != null) ? list.ToString() : null;
        }
        #endregion
    }
}
