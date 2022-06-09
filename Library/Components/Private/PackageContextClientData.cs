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

using PackageIndexPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Containers.Private.PackageWrapperDictionary>;

using PackageIndexWrapperDictionary = Eagle._Containers.Public.PathDictionary<
    Eagle._Containers.Private.PackageWrapperDictionary>;

namespace Eagle._Components.Private
{
    [ObjectId("47a703f1-8707-44f9-8d92-39c7ce167925")]
    internal sealed class PackageContextClientData : ClientData
    {
        #region Private Constructors
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
        public PackageContextClientData()
            : this(null)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
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
        private string indexFileName;
        public string IndexFileName
        {
            get { return indexFileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PackageIndexWrapperDictionary indexedPackages;
        public PackageIndexWrapperDictionary IndexedPackages
        {
            get { return indexedPackages; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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

        public bool HasPackages(
            ref Result error /* out */
            )
        {
            PackageWrapperDictionary packages = GetPackages(
                indexFileName, false, ref error);

            return (packages != null);
        }

        ///////////////////////////////////////////////////////////////////////

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

            _Wrappers.Package wrapper;

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
            _Wrappers.Package wrapper = null;

            try
            {
                long id = EntityOps.NextTokenIdNoThrow(package);
                wrapper = new _Wrappers.Package(id, package);

                packages.Add(name, wrapper);
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
        public override string ToString()
        {
            StringList list = ToList(null, false);

            return (list != null) ? list.ToString() : null;
        }
        #endregion
    }
}
