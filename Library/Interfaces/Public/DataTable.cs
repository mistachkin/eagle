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

namespace Eagle._Interfaces.Public
{
    [ObjectId("438264c5-9362-4cd1-8149-61deaeac4e44")]
    public interface IDataTable
    {
        IStringList ToList();
        IStringList ToList(int limit);
        IStringList ToList(string filter, string sort);
        IStringList ToList(string filter, string sort, int limit);

        ///////////////////////////////////////////////////////////////////////

        IStringList ToDictionary();
        IStringList ToDictionary(int limit);
        IStringList ToDictionary(string filter, string sort);
        IStringList ToDictionary(string filter, string sort, int limit);

        ///////////////////////////////////////////////////////////////////////

        IStringList GetColumnNames();
    }
}
