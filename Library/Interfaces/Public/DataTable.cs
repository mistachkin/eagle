/*
 * DataTable.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("438264c5-9362-4cd1-8149-61deaeac4e44")]
    public interface IDataTable
    {
        StringList ToList();
        StringList ToList(int limit);
        StringList ToList(string filterExpression, string sort);
        StringList ToList(string filterExpression, string sort,
            int limit);

        ///////////////////////////////////////////////////////////////////////

        StringList ToDictionary();
        StringList ToDictionary(int limit);
        StringList ToDictionary(string filterExpression,
            string sort);
        StringList ToDictionary(string filterExpression,
            string sort, int limit);

        ///////////////////////////////////////////////////////////////////////

        StringList GetColumnNames();
    }
}
