/*
 * SupportVariable.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that provide helper methods
    /// used to support array variable sub-commands, including existence
    /// checks, element counting, and listing or formatting of the keys and
    /// values of an array.
    /// </summary>
    [ObjectId("485b2465-1daf-48f4-9a5c-802978eeea41")]
    public interface ISupportVariable
    {
        #region Array Sub-Command Helper Methods
        /// <summary>
        /// Determines whether the underlying variable exists.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for this operation.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the variable to check for existence.  This parameter
        /// should not be null.
        /// </param>
        /// <returns>
        /// True if the variable exists; otherwise, false.
        /// </returns>
        bool DoesExist(
            Interpreter interpreter,
            string name
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of elements in the underlying array variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for this operation.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The number of elements, or null if the count could not be
        /// determined.
        /// </returns>
        long? GetCount(
            Interpreter interpreter,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a dictionary of the names and/or values of the elements in the
        /// underlying array variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for this operation.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the element names in the resulting dictionary.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the element values in the resulting dictionary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The dictionary of element names and/or values, or null upon
        /// failure.
        /// </returns>
        ObjectDictionary GetList(
            Interpreter interpreter,
            bool names,
            bool values,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a dictionary of the names and/or values of the elements in the
        /// underlying array variable whose names match the specified pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for this operation.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which element names are included, or null
        /// to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the element names in the resulting dictionary.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the element values in the resulting dictionary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The dictionary of element names and/or values, or null upon
        /// failure.
        /// </returns>
        ObjectDictionary GetList(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            bool names,
            bool values,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the names of the elements in the underlying array variable
        /// whose names match the specified pattern as a string.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for this operation.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="mode">
        /// The <see cref="MatchMode" /> value that controls how the pattern is
        /// interpreted.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which element names are included, or null
        /// to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The <see cref="RegexOptions" /> to use when the match mode specifies
        /// regular expression matching.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The formatted string of element names, or null upon failure.
        /// </returns>
        string KeysToString(
            Interpreter interpreter,
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the names and values of the elements in the underlying
        /// array variable whose names match the specified pattern as a string.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for this operation.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which element names are included, or null
        /// to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The formatted string of element names and values, or null upon
        /// failure.
        /// </returns>
        string KeysAndValuesToString(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            ref Result error
        );
        #endregion
    }
}
