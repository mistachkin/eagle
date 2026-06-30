/*
 * PathClientDataDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using PathClientDataPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Components.Private.PathClientData>;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary of path-related client data, keyed by
    /// path or name.  It extends the path dictionary with a type name suitable
    /// for use within Eagle and the ability to add entries keyed by their own
    /// name or path and to produce a list of their string representations.
    /// </summary>
    [ObjectId("f996e29a-c6cd-46be-8b54-1c9e37e05cae")]
    internal sealed class PathClientDataDictionary :
            PathDictionary<PathClientData>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public PathClientDataDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// Gets the dictionary key for the specified client data, using either
        /// its name or its path.
        /// </summary>
        /// <param name="clientData">
        /// The client data whose key is to be returned.
        /// </param>
        /// <param name="all">
        /// Non-zero to use the name of the client data as the key; zero to use
        /// its path.
        /// </param>
        /// <returns>
        /// The dictionary key for the specified client data, or null if it is
        /// null.
        /// </returns>
        private static string GetKey(
            PathClientData clientData,
            bool all
            )
        {
            if (clientData == null)
                return null;

            return all ? clientData.Name : clientData.Path;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Adds the specified client data to this dictionary, keyed by its own
        /// name or path, unless its key is null or already present.
        /// </summary>
        /// <param name="clientData">
        /// The client data to be added to this dictionary.
        /// </param>
        /// <param name="all">
        /// Non-zero to use the name of the client data as the key; zero to use
        /// its path.
        /// </param>
        public void Add(
            PathClientData clientData,
            bool all
            )
        {
            string key = GetKey(clientData, all);

            if (key == null)
                return;

            if (ContainsKey(key))
                return;

            Add(key, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a list of the string representations of the client data
        /// stored in this dictionary.
        /// </summary>
        /// <returns>
        /// A list containing the string representation of each non-null client
        /// data value in this dictionary.
        /// </returns>
        public IStringList ToList()
        {
            IStringList list = new StringPairList();

            foreach (PathClientDataPair pair in this)
            {
                PathClientData clientData = pair.Value;

                if (clientData == null)
                    continue;

                list.Add(clientData.ToString());
            }

            return list;
        }
        #endregion
    }
}
