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
    [ObjectId("f996e29a-c6cd-46be-8b54-1c9e37e05cae")]
    internal sealed class PathClientDataDictionary :
            PathDictionary<PathClientData>
    {
        #region Public Constructors
        public PathClientDataDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
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
