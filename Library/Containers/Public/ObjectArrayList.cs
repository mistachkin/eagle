/*
 * ObjectArrayList.cs --
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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("d29cbbf9-8a2e-40b4-bbc1-903f3d6fdc95")]
    public sealed class ObjectArrayList : List<object[]>, ICloneable
    {
        public ObjectArrayList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectArrayList(
            IEnumerable<object[]> collection /* in */
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectArrayList(
            int capacity /* in */
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public ObjectArrayList(
            params object[][] objects /* in */
            )
            : base(objects)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = new StringList();

            foreach (object[] element in this)
            {
                if (element == null)
                    continue;

                StringList subList = new StringList();

                foreach (object subElement in element)
                {
                    if (subElement == null)
                        continue;

                    string subElementString =
                        StringOps.GetStringFromObject(subElement);

                    if ((pattern != null) && !StringOps.Match(
                            null, StringOps.DefaultMatchMode,
                            subElementString, pattern, noCase))
                    {
                        continue;
                    }

                    subList.Add(subElementString);
                }

                list.Add(subList.ToString());
            }

            return list.ToString();
        }

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
            return new ObjectArrayList(this);
        }
        #endregion
    }
}
