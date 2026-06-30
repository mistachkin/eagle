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
    /// <summary>
    /// This interface is implemented by objects that wrap a table of data and
    /// can render its rows and columns as lists or dictionaries, optionally
    /// filtered, sorted, and limited in size.
    /// </summary>
    [ObjectId("438264c5-9362-4cd1-8149-61deaeac4e44")]
    public interface IDataTable
    {
        /// <summary>
        /// Converts all rows of the table to a list.
        /// </summary>
        /// <returns>
        /// A list containing the rows of the table, or null if the
        /// conversion fails.
        /// </returns>
        IStringList ToList();

        /// <summary>
        /// Converts the rows of the table to a list, limiting the number of
        /// rows returned.
        /// </summary>
        /// <param name="limit">
        /// The maximum number of rows to include in the resulting list.
        /// </param>
        /// <returns>
        /// A list containing the selected rows of the table, or null if the
        /// conversion fails.
        /// </returns>
        IStringList ToList(int limit);

        /// <summary>
        /// Converts the rows of the table to a list, filtering and sorting
        /// the rows.
        /// </summary>
        /// <param name="filter">
        /// The expression used to select which rows are included, or null to
        /// include all rows.
        /// </param>
        /// <param name="sort">
        /// The expression used to order the rows, or null to use the natural
        /// order.
        /// </param>
        /// <returns>
        /// A list containing the selected rows of the table, or null if the
        /// conversion fails.
        /// </returns>
        IStringList ToList(string filter, string sort);

        /// <summary>
        /// Converts the rows of the table to a list, filtering and sorting
        /// the rows and limiting the number returned.
        /// </summary>
        /// <param name="filter">
        /// The expression used to select which rows are included, or null to
        /// include all rows.
        /// </param>
        /// <param name="sort">
        /// The expression used to order the rows, or null to use the natural
        /// order.
        /// </param>
        /// <param name="limit">
        /// The maximum number of rows to include in the resulting list.
        /// </param>
        /// <returns>
        /// A list containing the selected rows of the table, or null if the
        /// conversion fails.
        /// </returns>
        IStringList ToList(string filter, string sort, int limit);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts all rows of the table to a dictionary of column name and
        /// value pairs.
        /// </summary>
        /// <returns>
        /// A list of column name and value pairs for the rows of the table,
        /// or null if the conversion fails.
        /// </returns>
        IStringList ToDictionary();

        /// <summary>
        /// Converts the rows of the table to a dictionary of column name and
        /// value pairs, limiting the number of rows returned.
        /// </summary>
        /// <param name="limit">
        /// The maximum number of rows to include in the result.
        /// </param>
        /// <returns>
        /// A list of column name and value pairs for the selected rows of the
        /// table, or null if the conversion fails.
        /// </returns>
        IStringList ToDictionary(int limit);

        /// <summary>
        /// Converts the rows of the table to a dictionary of column name and
        /// value pairs, filtering and sorting the rows.
        /// </summary>
        /// <param name="filter">
        /// The expression used to select which rows are included, or null to
        /// include all rows.
        /// </param>
        /// <param name="sort">
        /// The expression used to order the rows, or null to use the natural
        /// order.
        /// </param>
        /// <returns>
        /// A list of column name and value pairs for the selected rows of the
        /// table, or null if the conversion fails.
        /// </returns>
        IStringList ToDictionary(string filter, string sort);

        /// <summary>
        /// Converts the rows of the table to a dictionary of column name and
        /// value pairs, filtering and sorting the rows and limiting the
        /// number returned.
        /// </summary>
        /// <param name="filter">
        /// The expression used to select which rows are included, or null to
        /// include all rows.
        /// </param>
        /// <param name="sort">
        /// The expression used to order the rows, or null to use the natural
        /// order.
        /// </param>
        /// <param name="limit">
        /// The maximum number of rows to include in the result.
        /// </param>
        /// <returns>
        /// A list of column name and value pairs for the selected rows of the
        /// table, or null if the conversion fails.
        /// </returns>
        IStringList ToDictionary(string filter, string sort, int limit);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the names of the columns in the table.
        /// </summary>
        /// <returns>
        /// A list containing the column names of the table, or null if they
        /// are not available.
        /// </returns>
        IStringList GetColumnNames();
    }
}
